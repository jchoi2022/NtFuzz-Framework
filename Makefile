BUILDDIR=$(shell pwd)/build

all: FuzzingFramework

FuzzingFramework:
		@mkdir -p $(BUILDDIR)
		@dotnet build -c Debug -o $(BUILDDIR) src/FuzzingFramework.fsproj

clean:
		@dotnet clean -c Debug
		@rm -rf $(BUILDDIR)

.PHONY: FuzzingFramework all clean

